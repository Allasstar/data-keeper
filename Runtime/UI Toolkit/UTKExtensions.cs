using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for specific UI element types like Button, Image, TextField, etc.
    /// </summary>
    public static class UTKExtensions
    {
        #region Button Extensions

        public static T SetOnClicked<T>(this T button, Action callback) where T : Button
        {
            button.clicked += callback;
            return button;
        }

        public static T SetContent<T>(this T button, string text, Action callback) where T : Button
        {
            button.text = text;
            button.clicked += callback;
            return button;
        }

        public static T SetButtonText<T>(this T button, string text) where T : Button
        {
            button.text = text;
            return button;
        }

        #endregion

        #region Image Extensions

        public static T SetVectorImage<T>(this T element, VectorImage vectorImage) where T : Image
        {
            element.vectorImage = vectorImage;
            return element;
        }

        public static T SetImage<T>(this T element, Sprite sprite) where T : Image
        {
            element.sprite = sprite;
            return element;
        }

        public static T SetImage<T>(this T element, Texture2D texture) where T : Image
        {
            element.image = texture;
            return element;
        }

        public static T SetImageTint<T>(this T image, Color tint) where T : Image
        {
            image.tintColor = tint;
            return image;
        }

        public static T SetScaleMode<T>(this T image, ScaleMode scaleMode) where T : Image
        {
            image.scaleMode = scaleMode;
            return image;
        }

        public static T SetUV<T>(this T image, Rect uv) where T : Image
        {
            image.uv = uv;
            return image;
        }

        public static T SetImageScaleMode<T>(this T image, ScaleMode scaleMode) where T : Image
        {
            image.scaleMode = scaleMode;
            return image;
        }

        #endregion

        #region Label Extensions

        public static T SetLabelText<T>(this T label, string text) where T : Label
        {
            label.text = text;
            return label;
        }

        #endregion

        #region TextField Extensions

        public static T SetValue<T>(this T textField, string value) where T : TextField
        {
            textField.value = value;
            return textField;
        }

        public static T SetMaxLength<T>(this T textField, int maxLength) where T : TextField
        {
            textField.maxLength = maxLength;
            return textField;
        }

        public static T SetMultiline<T>(this T textField, bool multiline = true) where T : TextField
        {
            textField.multiline = multiline;
            return textField;
        }

        public static T SetPasswordField<T>(this T textField, bool isPassword = true) where T : TextField
        {
            textField.isPasswordField = isPassword;
            return textField;
        }

        public static T SetReadOnly<T>(this T textField, bool readOnly = true) where T : TextField
        {
            textField.isReadOnly = readOnly;
            return textField;
        }

        public static T SetOnValueChanged<T>(this T textField, EventCallback<ChangeEvent<string>> callback) where T : TextField
        {
            textField.RegisterValueChangedCallback(callback);
            return textField;
        }

        #endregion

        #region Toggle Extensions

        public static T SetToggleValue<T>(this T toggle, bool value) where T : Toggle
        {
            toggle.value = value;
            return toggle;
        }

        public static T SetToggleText<T>(this T toggle, string text) where T : Toggle
        {
            toggle.text = text;
            return toggle;
        }

        public static T SetOnToggleChanged<T>(this T toggle, EventCallback<ChangeEvent<bool>> callback) where T : Toggle
        {
            toggle.RegisterValueChangedCallback(callback);
            return toggle;
        }

        #endregion

        #region Slider Extensions

        public static T SetSliderValue<T>(this T slider, float value) where T : Slider
        {
            slider.value = value;
            return slider;
        }

        public static T SetSliderRange<T>(this T slider, float min, float max) where T : Slider
        {
            slider.lowValue = min;
            slider.highValue = max;
            return slider;
        }

        public static T SetSliderDirection<T>(this T slider, SliderDirection direction) where T : Slider
        {
            slider.direction = direction;
            return slider;
        }

        public static T SetOnSliderChanged<T>(this T slider, EventCallback<ChangeEvent<float>> callback) where T : Slider
        {
            slider.RegisterValueChangedCallback(callback);
            return slider;
        }

        #endregion

        #region ProgressBar Extensions

        public static T SetProgress<T>(this T progressBar, float value) where T : ProgressBar
        {
            progressBar.value = value;
            return progressBar;
        }

        public static T SetProgressRange<T>(this T progressBar, float min, float max) where T : ProgressBar
        {
            progressBar.lowValue = min;
            progressBar.highValue = max;
            return progressBar;
        }

        public static T SetProgressTitle<T>(this T progressBar, string title) where T : ProgressBar
        {
            progressBar.title = title;
            return progressBar;
        }

        #endregion

        #region DropdownField Extensions

        public static T SetDropdownChoices<T>(this T dropdown, List<string> choices) where T : DropdownField
        {
            dropdown.choices = choices;
            return dropdown;
        }

        public static T SetDropdownValue<T>(this T dropdown, string value) where T : DropdownField
        {
            dropdown.value = value;
            return dropdown;
        }

        public static T SetDropdownIndex<T>(this T dropdown, int index) where T : DropdownField
        {
            dropdown.index = index;
            return dropdown;
        }

        public static T SetOnDropdownChanged<T>(this T dropdown, EventCallback<ChangeEvent<string>> callback) where T : DropdownField
        {
            dropdown.RegisterValueChangedCallback(callback);
            return dropdown;
        }

        #endregion

        #region ScrollView Extensions

        public static T SetScrollViewMode<T>(this T scrollView, ScrollViewMode mode) where T : ScrollView
        {
            scrollView.mode = mode;
            return scrollView;
        }

        public static T SetScrollDecelerationRate<T>(this T scrollView, float rate) where T : ScrollView
        {
            scrollView.scrollDecelerationRate = rate;
            return scrollView;
        }

        public static T SetElasticity<T>(this T scrollView, float elasticity) where T : ScrollView
        {
            scrollView.elasticity = elasticity;
            return scrollView;
        }

        #endregion

        #region ListView Extensions

        public static T SetListViewItemHeight<T>(this T listView, float height) where T : ListView
        {
            listView.fixedItemHeight = height;
            return listView;
        }

        public static T SetListViewItemSource<T>(this T listView, IList itemsSource) where T : ListView
        {
            listView.itemsSource = itemsSource;
            return listView;
        }

        public static T SetListViewMakeItem<T>(this T listView, Func<VisualElement> makeItem) where T : ListView
        {
            listView.makeItem = makeItem;
            return listView;
        }

        public static T SetListViewBindItem<T>(this T listView, Action<VisualElement, int> bindItem) where T : ListView
        {
            listView.bindItem = bindItem;
            return listView;
        }

        public static T SetListViewSelectionType<T>(this T listView, SelectionType selectionType) where T : ListView
        {
            listView.selectionType = selectionType;
            return listView;
        }

        public static T SetOnListViewSelectionChanged<T>(this T listView, Action<IEnumerable<object>> callback) where T : ListView
        {
            listView.selectionChanged += callback;
            return listView;
        }

        #endregion

        #region Foldout Extensions

        public static T SetFoldoutText<T>(this T foldout, string text) where T : Foldout
        {
            foldout.text = text;
            return foldout;
        }

        public static T SetFoldoutValue<T>(this T foldout, bool value) where T : Foldout
        {
            foldout.value = value;
            return foldout;
        }

        public static T SetOnFoldoutChanged<T>(this T foldout, EventCallback<ChangeEvent<bool>> callback) where T : Foldout
        {
            foldout.RegisterValueChangedCallback(callback);
            return foldout;
        }

        #endregion

        #region GroupBox Extensions

        public static T SetGroupBoxText<T>(this T groupBox, string text) where T : GroupBox
        {
            groupBox.text = text;
            return groupBox;
        }

        #endregion
    }
}