using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SP1Assets.MonsterPack2D
{
    [AddComponentMenu("Layout/Centerd Grid Layout Group")]
    public class CenteredGridLayoutGroup : GridLayoutGroup
    {
        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();
            CenterLastRow();
        }

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();
            CenterLastRow();
        }

        void CenterLastRow()
        {
            if (constraint != Constraint.FixedColumnCount || constraintCount <= 0 || rectChildren.Count == 0)
                return;

            int total = rectChildren.Count;
            int fullRows = total / constraintCount;
            int itemsInLastRow = total % constraintCount;

            if (itemsInLastRow == 0)
                return;

            float offset = (cellSize.x + spacing.x) * (constraintCount - itemsInLastRow) * 0.5f;

            for (int i = fullRows * constraintCount; i < total; i++)
            {
                RectTransform item = rectChildren[i];
                Vector3 pos = item.anchoredPosition;

                switch (startCorner)
                {
                    case Corner.UpperLeft:
                    case Corner.LowerLeft:
                        pos.x += offset;
                        break;
                    case Corner.UpperRight:
                    case Corner.LowerRight:
                        pos.x -= offset;
                        break;
                }

                item.anchoredPosition = pos;
            }
        }
    }
}