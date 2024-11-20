using System;
using UnityEngine;

namespace DataKeeper.Extra
{
    public class MaterialAction
    {
        private MaterialPropertyBlock _propertyBlock;
        private MeshRenderer _meshRenderer;

        public MaterialAction(MeshRenderer meshRenderer)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _meshRenderer = meshRenderer;
        }

        public void Act(Action<MaterialPropertyBlock> property)
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            property(_propertyBlock);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        
        public void Act(int materialIndex, Action<MaterialPropertyBlock> property)
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock, materialIndex);
            property(_propertyBlock);
            _meshRenderer.SetPropertyBlock(_propertyBlock, materialIndex);
        }
    }
}
