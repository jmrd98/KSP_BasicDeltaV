﻿
using System.Collections.Generic;
using UnityEngine;
using BasicDeltaV.Unity.Unity;

namespace BasicDeltaV.Unity
{
    public class BasicDVPanelManager : MonoBehaviour
    {
        List<BasicDeltaV_SimplePanel> _activeSimplePanels = new List<BasicDeltaV_SimplePanel>();

        private int _updateCounter;

        private static BasicDVPanelManager _instance;

        public static BasicDVPanelManager Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            
            _activeSimplePanels.Clear();
        }
        
        public void RegisterPanel(BasicDeltaV_SimplePanel panel)
        {
            if (!_activeSimplePanels.Contains(panel))
                _activeSimplePanels.Add(panel);
        }

        public void UnregisterPanel(BasicDeltaV_SimplePanel panel)
        {
            if (_activeSimplePanels.Contains(panel))
                _activeSimplePanels.Remove(panel);
        }
        
        private void Update()
        {
            if (_activeSimplePanels == null || _activeSimplePanels.Count <= 0)
                return;

            _updateCounter++;

            if (_updateCounter >= _activeSimplePanels.Count)
                _updateCounter = 0;

            _activeSimplePanels[_updateCounter].OnUpdate();
        }
    }
}
