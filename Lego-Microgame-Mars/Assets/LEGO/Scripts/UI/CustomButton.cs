using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.LEGO.UI
{
    public class CustomButton : Button
    {
        public UnityEvent OnCustomClick;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            OnCustomClick?.Invoke();
        }
    }
}
