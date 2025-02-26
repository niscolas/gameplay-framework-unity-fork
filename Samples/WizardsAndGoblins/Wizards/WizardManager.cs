﻿using UnityEngine;

namespace MiddleMast.GameplayFramework.Samples.WizardsAndGoblins.Wizards
{
    internal class WizardManager : Manager
    {
        [SerializeField] private Wizard _wizardPrefab;

        private ISpellFactory _spellFactory;
        private Wizard _wizard;

        public void Setup(ISpellFactory spellFactory)
        {
            _spellFactory = spellFactory;

            CreateWizard();
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            _wizard.Tick(deltaTime);
        }

        private void CreateWizard()
        {
            _wizard = Instantiate(_wizardPrefab);
            _wizard.Setup(_spellFactory);
        }
    }
}
