using System;
using System.Collections.Generic;
using System.Linq;
using SvgEditor.Core.Svg;
using SvgEditor.UI.Inspector.State;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal sealed class TargetSelectionState
    {
        private readonly Dictionary<string, string> _labelsByKey = new(StringComparer.Ordinal);
        public string SelectedTargetKey { get; private set; } = SvgTargets.RootTargetKey;
        public string SelectedTargetLabel => ResolveSelectedTargetLabel();

        public void SetTargets(IReadOnlyList<PatchTarget> targets)
        {
            var previousSelectedKey = SelectedTargetKey;
            _labelsByKey.Clear();
            _labelsByKey[SvgTargets.RootTargetKey] = "Root <svg>";

            if (targets != null)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    if (string.IsNullOrWhiteSpace(target?.DisplayName) || string.IsNullOrWhiteSpace(target.Key))
                        continue;

                    _labelsByKey[target.Key] = target.DisplayName;
                }
            }

            SelectedTargetKey = _labelsByKey.ContainsKey(previousSelectedKey)
                ? previousSelectedKey
                : SvgTargets.RootTargetKey;
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(targetKey))
                return false;

            if (!_labelsByKey.TryGetValue(targetKey, out label))
                return false;

            SelectedTargetKey = targetKey;
            return true;
        }

        public string ResolveSelectedTargetKey()
        {
            return _labelsByKey.ContainsKey(SelectedTargetKey)
                ? SelectedTargetKey
                : SvgTargets.RootTargetKey;
        }

        private string ResolveSelectedTargetLabel()
        {
            return _labelsByKey.TryGetValue(ResolveSelectedTargetKey(), out var label)
                ? label
                : "Root <svg>";
        }
    }
}
