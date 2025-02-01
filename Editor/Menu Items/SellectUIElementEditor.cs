using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.MenuItems
{
    public class SelectUIElementEditor : UnityEditor.Editor
    {
        [MenuItem("Tools/Select UI _PGUP", priority = 2)]
        private static void Select()
        {
            Debug.Log("Select");
            List<RaycastResult> rr = RaycastMouse();

            GameObject selectThis = null;
        
            if (rr.Count > 0)
            {
                selectThis = rr[0].gameObject;
            }

            foreach (RaycastResult result in rr)
            {
                var selectable = result.gameObject.GetComponent<Selectable>();
                if (selectable != null)
                {
                    selectThis = selectable.gameObject;
                    break;
                }
            
                var parentSelectable = result.gameObject.transform.parent.gameObject.GetComponent<Selectable>();
                if (parentSelectable != null)
                {
                    selectThis = parentSelectable.gameObject;
                    break;
                }
            }

            if (selectThis != null)
            {
                Selection.objects = new Object[] {selectThis};
            }
        }

        private static List<RaycastResult> RaycastMouse()
        {
            if (EventSystem.current == null)
            {
                Debug.Log("It works only in Play Mode in Game View");
                return new List<RaycastResult>();
            }
            
            var pointerData = new PointerEventData(EventSystem.current)
            {
                pointerId = -1, 
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
         
            return results;
        }
    }
}