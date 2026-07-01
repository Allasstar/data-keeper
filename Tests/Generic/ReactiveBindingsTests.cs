using DataKeeper.Generic;
using DataKeeper.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Tests.Generic
{
    public class ReactiveBindingsTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("bindings_test");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void FloatSlider_TwoWay()
        {
            var slider = _go.AddComponent<Slider>();
            var reactive = new Reactive<float>(0.25f);

            reactive.BindTo(slider);

            Assert.AreEqual(0.25f, slider.value); // initial push

            reactive.Value = 0.75f;
            Assert.AreEqual(0.75f, slider.value); // reactive -> UI

            slider.value = 0.5f;
            Assert.AreEqual(0.5f, reactive.Value); // UI -> reactive
        }

        [Test]
        public void FloatSlider_OneWay_DoesNotWriteBack()
        {
            var slider = _go.AddComponent<Slider>();
            var reactive = new Reactive<float>(0.25f);

            reactive.BindTo(slider, twoWay: false);

            slider.value = 0.9f;
            Assert.AreEqual(0.25f, reactive.Value);
        }

        [Test]
        public void IntSlider_RoundsOnWriteBack()
        {
            var slider = _go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 10f;
            var reactive = new Reactive<int>(2);

            reactive.BindTo(slider);

            Assert.AreEqual(2f, slider.value);

            slider.value = 6.7f;
            Assert.AreEqual(7, reactive.Value);
        }

        [Test]
        public void Toggle_TwoWay()
        {
            var toggle = _go.AddComponent<Toggle>();
            var reactive = new Reactive<bool>(true);

            reactive.BindTo(toggle);

            Assert.IsTrue(toggle.isOn);

            reactive.Value = false;
            Assert.IsFalse(toggle.isOn);

            toggle.isOn = true;
            Assert.IsTrue(reactive.Value);
        }

        [Test]
        public void BindToFill_UpdatesImage()
        {
            var image = _go.AddComponent<Image>();
            var reactive = new Reactive<float>(0.3f);

            reactive.BindToFill(image);
            Assert.AreEqual(0.3f, image.fillAmount);

            reactive.Value = 0.8f;
            Assert.AreEqual(0.8f, image.fillAmount);
        }

        [Test]
        public void BindToColor_UpdatesGraphic()
        {
            var image = _go.AddComponent<Image>();
            var reactive = new Reactive<Color>(Color.red);

            reactive.BindToColor(image);
            Assert.AreEqual(Color.red, image.color);

            reactive.Value = Color.green;
            Assert.AreEqual(Color.green, image.color);
        }

        [Test]
        public void BindToActive_TogglesGameObject()
        {
            var target = new GameObject("target");
            try
            {
                var reactive = new Reactive<bool>(false);

                reactive.BindToActive(target);
                Assert.IsFalse(target.activeSelf);

                reactive.Value = true;
                Assert.IsTrue(target.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void BindToAlpha_UpdatesCanvasGroup()
        {
            var group = _go.AddComponent<CanvasGroup>();
            var reactive = new Reactive<float>(0.5f);

            reactive.BindToAlpha(group);
            Assert.AreEqual(0.5f, group.alpha);

            reactive.Value = 1f;
            Assert.AreEqual(1f, group.alpha);
        }

        [Test]
        public void Dispose_StopsUpdates()
        {
            var slider = _go.AddComponent<Slider>();
            var reactive = new Reactive<float>(0.2f);

            var binding = reactive.BindTo(slider);
            binding.Dispose();

            reactive.Value = 0.9f;
            Assert.AreEqual(0.2f, slider.value);

            slider.value = 0.6f;
            Assert.AreEqual(0.9f, reactive.Value); // write-back also unbound
        }

        [Test]
        public void DestroyingTarget_ReleasesBinding()
        {
            var reactive = new Reactive<int>(0);
            int applied = 0;

            reactive.Bind(_go.transform, _ => applied++);
            Assert.AreEqual(1, applied); // initial push

            reactive.Value = 1;
            Assert.AreEqual(2, applied);

            Object.DestroyImmediate(_go);
            _go = null;

            reactive.Value = 2;
            Assert.AreEqual(2, applied); // unbound via lifetime component
        }

        [Test]
        public void ReactivePref_ImplementsIReactiveT()
        {
            // Compile-time contract: ReactivePref<T> is usable wherever bindings expect IReactive<T>.
            PlayerPrefs.DeleteKey("test_binding_pref");
            IReactive<float> pref = new ReactivePref<float>(0.5f, "test_binding_pref", autoSave: false);
            Assert.AreEqual(0.5f, pref.Value);
        }
    }
}
