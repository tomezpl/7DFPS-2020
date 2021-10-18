using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class DeathMatchGameMode : MultiplayerGameMode
{
    public override string Name { get => "Deathmatch"; }

    public void Update()
    {
        
    }

    protected override Type GetExtensionsType() => typeof(DeathMatchGameManagerExtensions);
}
