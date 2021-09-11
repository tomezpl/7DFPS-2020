using UnityEngine;

public class MuzzleFlashBillboard : SimpleBillboard
{
    protected override void UpdateShader()
    {
        if (billboardMaterial && transform.parent)
        {
            // Update the direction vector in the shader.
            // This might be accessible from one of the matrices but I didn't want to tinker too much in ShaderLab...
            billboardMaterial.SetVector(Shader.PropertyToID("_MuzzleFlashDirection"), transform.parent.forward);
        }
    }
}
