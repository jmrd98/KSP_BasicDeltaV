﻿#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_AppLauncher - Stock app launcher controller
 * 
 * Copyright (C) 2016 DMagic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version. 
 * 
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details. 
 * 
 * You should have received a copy of the GNU General Public License 
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 * 
 * 
 */
#endregion

using System;
using System.Collections;
using BasicDeltaV.Unity.Unity;
using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

namespace BasicDeltaV
{
    public class BasicDeltaV_AppLauncher : MonoBehaviour
    {
        private ApplicationLauncherButton button;
        private IEnumerator buttonAdder;

        private static BasicDeltaV_AppLauncher instance;
        private static Texture2D icon;

        private bool _sticky;
        private bool _hovering;

        private BasicDeltaV_AppWindow launcher;

        public static BasicDeltaV_AppLauncher Instance
        {
            get { return instance; }
        }

		public BasicDeltaV_AppWindow Launcher
		{
			get { return launcher; }
		}

        private void Start()
        {
            if (icon == null)
            {
                icon = new Texture2D(38, 38, TextureFormat.ARGB32, false);

                string path = Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/BasicDeltaV/Resources/AppIcon.png").Replace("\\", "/");

                if (File.Exists(path))
                    icon.LoadImage(File.ReadAllBytes(path));
            }

            instance = this;

            if (buttonAdder != null)
                StopCoroutine(buttonAdder);

            buttonAdder = AddButton();
            StartCoroutine(buttonAdder);

            GameEvents.OnGameSettingsApplied.Add(Reposition);
        }

        private void OnDestroy()
        {
            if (launcher != null)
                Destroy(launcher.gameObject);

            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveButton);
            GameEvents.OnGameSettingsApplied.Remove(Reposition);
        }

        private IEnumerator AddButton()
        {
            while (!ApplicationLauncher.Ready)
                yield return null;

            while (ApplicationLauncher.Instance == null)
                yield return null;

            button = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, OnHover, OnHoverOut, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT, icon);

            ApplicationLauncher.Instance.EnableMutuallyExclusive(button);

            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButton);

			button.onRightClick = (Callback)Delegate.Combine(button.onRightClick, new Callback(OnRightClick));

            if (HighLogic.LoadedSceneIsEditor)
            {
                button.toggleButton.onClick.RemoveAllListeners();
                button.toggleButton.onClick.AddListener(new UnityEngine.Events.UnityAction<PointerEventData, UIRadioButton.State, UIRadioButton.CallType>(OnClick));
            }

			if (!BasicDeltaV.ReadoutsAvailable)
				button.Disable();

            buttonAdder = null;
        }

        private void RemoveButton(GameScenes scene)
        {
            if (button == null)
                return;

            ApplicationLauncher.Instance.RemoveModApplication(button);
            button = null;
        }

		public void ToggleButtonState(bool isOn)
		{
            if (isOn)
                button.Enable();
            else
            {
                Close();

                button.Disable();
            }
		}

        private void Reposition()
        {
            if (launcher == null)
                return;

            launcher.transform.position = GetAnchor();
        }

        private void OnClick(PointerEventData data, UIRadioButton.State state, UIRadioButton.CallType callType)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                button.onLeftClick();
                button.onLeftClickBtn(button.toggleButton);
            }
            else if (data.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleClick();
            }
            else if (GameSettings.MODIFIER_KEY.GetKey(false) && data.button == PointerEventData.InputButton.Right)
            {
                OnMiddleClick();
            }
            else if (data.button == PointerEventData.InputButton.Right)
            {
                button.onRightClick();
            }
        }

        private void OnMiddleClick()
        {
            if (!BasicDeltaV.Instance.ShowAtmosphere)
                return;

            BasicDeltaV.Instance.Atmosphere = !BasicDeltaV.Instance.Atmosphere;

            if (launcher != null)
                launcher.SetAtmosphereToggle(BasicDeltaV.Instance.Atmosphere);
        }

		private void OnRightClick()
		{
			BasicDeltaV.Instance.DisplayActive = !BasicDeltaV.Instance.DisplayActive;

			if (launcher != null)
				launcher.SetDisplayToggle(BasicDeltaV.Instance.DisplayActive);
		}

        private void OnTrue()
        {
            _sticky = true;

            Open();
        }

        private void OnFalse()
        {
            Close();
        }
        
        private void OnHover()
        {
            _hovering = true;

            if (_sticky || launcher != null)
                return;

            Open();
        }

        private void OnHoverOut()
        {
            _hovering = false;

            if (!_sticky)
                StartCoroutine(HoverOutWait());
        }

        private IEnumerator HoverOutWait()
        {
            int timer = 0;

            while (timer < 2)
            {
                timer++;
                yield return null;
            }

            if (!BasicDeltaV.Instance.InMenu)
                Close();
        }

        public IEnumerator MenuHoverOutWait()
        {
            int timer = 0;

            while (timer < 2)
            {
                timer++;
                yield return null;
            }

            if (!_hovering && !_sticky && !BasicDeltaV.Instance.InMenu)
                Close();
        }
        
        public Vector3 GetAnchor()
        {
            if (button == null)
                return Vector3.zero;

            Vector3 anchor = button.GetAnchor();

            anchor.y += 2;

            return anchor;
        }

        private void Open()
        {
            if (launcher != null)
                return;

            if (BasicDeltaV_Loader.ToolbarPrefab == null)
                return;

            launcher = (Instantiate(BasicDeltaV_Loader.ToolbarPrefab, GetAnchor(), Quaternion.identity) as GameObject).GetComponent<BasicDeltaV_AppWindow>();
            
            if (launcher == null)
                return;

            launcher.transform.SetParent(UIMasterController.Instance.appCanvas.transform);
            
            launcher.setBasic(BasicDeltaV.Instance);
        }

        private void Close()
        {
            _sticky = false;

            if (launcher == null)
                return;

            launcher.Close();

            launcher = null;
        }
    }
}