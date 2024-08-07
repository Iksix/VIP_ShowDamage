using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using VipCoreApi;
using static VipCoreApi.IVipCoreApi;

namespace VIP_ShowDamage;

public class VipShowDamage : BasePlugin
{
    public override string ModuleAuthor => "iks";
    public override string ModuleName => "[VIP] ShowDamage";
    public override string ModuleVersion => "v1.0.2";
    
    private IVipCoreApi? _api = null!;
    private ShowDamage? _showDamage = null!;
    
    private PluginCapability<IVipCoreApi> PluginCapability { get; } = new("vipcore:core");

    

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = PluginCapability.Get();
        if (_api == null) return;

        _api.OnCoreReady += () =>
        {
            _showDamage = new ShowDamage(_api, Localizer);

            RegisterEventHandler<EventPlayerHurt>(_showDamage.OnPlayerHurt);
            RegisterListener<Listeners.OnTick>(() => {
                _showDamage.ShowDamageMessage();
            });

            _api.RegisterFeature(_showDamage);
        };
    }

    public override void Unload(bool hotReload)
    {
        _api?.UnRegisterFeature(_showDamage);
    }
}

public class ShowDamage : VipFeatureBase
{
    public override string Feature => "ShowDamage";
    IStringLocalizer Localizer;

    public Dictionary<CCSPlayerController, string> messages = new();
    public Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> deleteTimers = new();
    CounterStrikeSharp.API.Modules.Timers.Timer timer;


    public ShowDamage(IVipCoreApi api, IStringLocalizer localizer) : base(api)
    {
        Localizer = localizer;
        this.timer = timer;
    }

    public void ShowDamageMessage()
    {
        foreach (var msg in messages)
        {
            PrintHtml(msg.Key, msg.Value);
        }
    }

    public void PrintHtml(CCSPlayerController player, string hudContent) // thn for deafps
        {
            var @event = new EventShowSurvivalRespawnStatus(false)
            {
                LocToken = hudContent,
                Duration = 5,
                Userid = player
            };
            @event.FireEvent(false);

            @event = null;
        }


    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var target = @event.Userid;

        if (attacker == null || !attacker.IsValid || attacker.IsBot || attacker == target) return HookResult.Continue;

        if (!PlayerHasFeature(attacker)) return HookResult.Continue;

        if (GetPlayerFeatureState(attacker) != IVipCoreApi.FeatureState.Enabled) return HookResult.Continue;

        if (attacker.TeamNum == target.TeamNum) return HookResult.Continue;
        
        int Damage = @event.DmgHealth;

        string message = Localizer["ShowDamageMsg"].ToString()
        .Replace("{name}", target.PlayerName)
        .Replace("{damage}", Damage.ToString())
        ;

        if (deleteTimers.ContainsKey(attacker))
        {
            deleteTimers[attacker].Kill();
            deleteTimers.Remove(attacker);
            messages.Remove(attacker);
        }

        messages.Add(attacker, message);

        deleteTimers.Add(attacker, new CounterStrikeSharp.API.Modules.Timers.Timer(1, () => {
            messages.Remove(attacker);
        }));


        return HookResult.Continue;
    }
    
}