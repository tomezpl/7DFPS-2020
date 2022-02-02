using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A <see cref="SimpleBillboard"/> that will display a muzzle flash shader placed around a gun barrel.
/// </summary>
public class MuzzleFlashBillboard : SimpleBillboard
{
    float muzzleFlashZOffset = 0f;
    public Transform Pivot;

    protected override void Start()
    {
        base.Start();

        muzzleFlashZOffset = (billboard.transform.position - transform.position).magnitude;

        if(Pivot == null)
        {
            Pivot = transform;
        }
    }

    protected override void UpdateShader()
    {
        if (billboardMaterial && transform.parent)
        {
            // Update the direction vector in the shader.
            // This might be accessible from one of the matrices but I didn't want to tinker too much in ShaderLab...
            billboardMaterial.SetVector(Shader.PropertyToID("_MuzzleFlashDirection"), Pivot.forward);
        }
    }

    protected override void BillboardRotation(Camera currentCam)
    {
        transform.rotation = Quaternion.LookRotation((currentCam.transform.position - transform.position).normalized, Pivot.up);
    }

    protected override void Align(ScriptableRenderContext context, Camera camera)
    {
        base.Align(context, camera);

        SlideAlongBarrel();
    }

    /// <summary>
    /// Checks for obstacles between the desired placement for the billboard and the "safe", clip-free placement.
    /// The billboard is then placed at the furthest possible point on that vector to avoid clipping into objects.
    /// </summary>
    private void SlideAlongBarrel()
    {
        Ray ray = new Ray(transform.position, Pivot.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, muzzleFlashZOffset * 1.05f, ~(1 >> LayerMask.NameToLayer("DynamicObjects"))))
        {
            billboard.transform.position = transform.position + Pivot.forward * Mathf.Lerp(0f, muzzleFlashZOffset * 0.7f, hit.distance);
        }
        else
        {
            billboard.transform.position = transform.position + Pivot.forward * muzzleFlashZOffset;
        }
    }
}
