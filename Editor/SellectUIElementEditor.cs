#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor
{
    [InitializeOnLoad]
    public class SelectUIElementEditor : UnityEditor.Editor
    {
        static SelectUIElementEditor()
        {
            if(Application.isEditor)
                EditorApplication.update += Ticker;
        }
    
        static void Ticker()
        {
            if (!IsKeyPressed()) return;
            if(IsPointerOverGameObject())
                Select();
        }
    
        private static void Select()
        {
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

        private static bool IsPointerOverGameObject()
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsKeyPressed()
        {
            return Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift);
        }

        private static List<RaycastResult> RaycastMouse()
        {
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
#endif
