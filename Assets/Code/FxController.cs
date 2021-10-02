using UnityEngine;

/// <summary>
/// <para>Base class for scripted effects that we want to trigger on objects (e.g. weapons, props etc.)</para>
/// <para>The logic for these objects' effects can therefore be separated into its own MonoBehaviour and just be triggered from the owner script.</para>
/// </summary>
public abstract class FxController : MonoBehaviour
{
    /// <summary>
    /// Triggers the effect. Normally this would either have an instant effect, 
    /// or act as an activator for an effect spanning multiple frames.
    /// </summary>
    public abstract void Trigger();
}
