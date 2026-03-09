using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSelectionState
    {
        private const string RootLabel = "Root <svg>";

        private readonly List<InspectorTargetOption> _targetOptions = new();
        private readonly List<string> _targetChoices = new();

        public IReadOnlyList<string> TargetChoices => _targetChoices;
        public string SelectedTargetKey { get; private set; } = AttributePatcher.ROOT_TARGET_KEY;
        public string SelectedTargetLabel => ResolveSelectedTargetLabel();

        public void SetTargets(IReadOnlyList<PatchTarget> targets)
        {
            var previousSelectedKey = SelectedTargetKey;
            _targetOptions.Clear();
            _targetChoices.Clear();

            if (targets != null)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    if (string.IsNullOrWhiteSpace(target?.DisplayName) || string.IsNullOrWhiteSpace(target.Key))
                        continue;

                    _targetOptions.Add(new InspectorTargetOption
                    {
                        Key = target.Key,
                        Label = target.DisplayName
                    });
                }
            }

            if (_targetOptions.Count == 0)
            {
                _targetOptions.Add(new InspectorTargetOption
                {
                    Key = AttributePatcher.ROOT_TARGET_KEY,
                    Label = RootLabel
                });
            }

            for (var i = 0; i < _targetOptions.Count; i++)
                _targetChoices.Add(_targetOptions[i].Label);

            if (TryGetTargetOptionByKey(previousSelectedKey, out var selectedOption))
                SelectedTargetKey = selectedOption.Key;
            else
                SelectedTargetKey = _targetOptions[0].Key;
        }

        public void SelectTargetLabel(string label)
        {
            if (TryGetTargetOptionByLabel(label, out var option))
                SelectedTargetKey = option.Key;
        }

        public string ResolveSelectedTargetKey()
        {
            return string.IsNullOrWhiteSpace(SelectedTargetKey)
                ? AttributePatcher.ROOT_TARGET_KEY
                : SelectedTargetKey;
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(targetKey))
                return false;

            if (!TryGetTargetOptionByKey(targetKey, out var option))
                return false;

            SelectedTargetKey = option.Key;
            label = option.Label;
            return true;
        }

        private string ResolveSelectedTargetLabel()
        {
            return TryGetTargetOptionByKey(SelectedTargetKey, out var option)
                ? option.Label
                : RootLabel;
        }

        private bool TryGetTargetOptionByKey(string targetKey, out InspectorTargetOption option)
        {
            option = _targetOptions.FirstOrDefault(candidate =>
                string.Equals(candidate.Key, targetKey, StringComparison.Ordinal));
            return option != null;
        }

        private bool TryGetTargetOptionByLabel(string label, out InspectorTargetOption option)
        {
            option = _targetOptions.FirstOrDefault(candidate =>
                string.Equals(candidate.Label, label, StringComparison.Ordinal));
            return option != null;
        }
    }
}
