using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Modularity;
using VipCoreApi;

namespace VIP_ShowDamage;

public class VipShowDamage : BasePlugin, IModulePlugin
{
    public override string ModuleAuthor => "iks";
    public override string ModuleName => "[VIP] ShowDamage";
    public override string ModuleVersion => "v1.0.2";
    
    private static readonly string Feature = "ShowDamage";
    private IVipCoreApi _api = null!;
    
    public void LoadModule(IApiProvider provider)
    {
        _api = provider.Get<IVipCoreApi>();
        _api.RegisterFeature(Feature);
    }
    
    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerHurt>((@event, info) =>
        {
            var attacker = @event.Attacker;
            if (!attacker.IsValid) return HookResult.Continue;
            if (attacker.PlayerName == @event.Userid.PlayerName) return HookResult.Continue;

            if (!_api.IsClientVip(attacker)) return HookResult.Continue;
            if (!_api.PlayerHasFeature(attacker, Feature)) return HookResult.Continue;
            if (_api.GetPlayerFeatureState(attacker, Feature) is IVipCoreApi.FeatureState.Disabled
                or IVipCoreApi.FeatureState.NoAccess) return HookResult.Continue;
            if(!_api.GetFeatureValue<bool>(attacker, Feature)) return HookResult.Continue;
            
            attacker.PrintToCenterHtml($" Нанесён урон: <font color='red'>{@event.DmgHealth}HP</font>");
            
            return HookResult.Continue;
        });
    }
}