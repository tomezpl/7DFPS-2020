using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Data object that will be sent in Photon Events to indicate damage dealt to a player.
/// </summary>
public class DamageData
{
    public int AttackerViewId;
    public int VictimViewId;
    public int DamageDealt;

    /// <summary>
    /// Converts the damage data to a Photon-serializable format (array).
    /// </summary>
    /// <returns>Returns an array containing the 3 properties in the order they appear in this class (attacker, victim, damage)</returns>
    public object[] ToArray()
    {
        return new object[] { AttackerViewId, VictimViewId, DamageDealt };
    }
}
