# Archipelago.RiskOfRain2 | ![Discord Shield](https://discordapp.com/api/guilds/731205301247803413/widget.png?style=shield)

This mod adds support to Risk of Rain 2 for playing as an Archipelago client. For more information on Archipelago head over to https://archipelago.gg or join our Discord.

Should be multiplayer compatible, but not rigorously tested. Be sure to scale up your YAML settings if you play in multiplayer. Only the host needs the mod.

## Gameplay 

The Risk of Rain 2 players send checks by causing items to spawn in-game. That means opening chests or killing bosses, generally. 
An item check is only sent out after a certain number of items are picked up. This count is configurable in the player's YAML.

### Achieving Victory or Defeat

Achieving victory is defined as beating Mithrix or being defeated during Commencement. Obliterating is NOT supported at this time. You are NOT expected to revisit the planet
through the primordial teleporter but you MAY do so. However, remember that victory can only be achieved in Commencement, so you'll be locking yourself in for another 5 levels.

Due to the nature of roguelike games, you can possibly die and lose your place completely. This is mitigated partly by the free grants of `Dio's Best Friend`
but it is still possible to lose. If you do lose, you can reconnect to the Archipelago server and start a new run. The server will send you the items you have
earned thus far, giving you a small boost to the start of your run.

## YAML Settings

An example YAML would look like this:
```yaml
description: Ijwu-ror2
name: Ijwu

game:
  Risk of Rain 2: 1

Risk of Rain 2:
  total_locations: 15
  total_revivals: 4
  start_with_revive: true
  item_pickup_step: 1
```

| Name | Description | Allowed values |
| ---- | ----------- | -------------- |
| total_locations | The total number of location checks that will be attributed to the Risk of Rain player. | 10 - 50 |
| total_revivals | The total number of items in the Risk of Rain player's item pool (items other players pick up for them) replaced with `Dio's Best Friend`. | 0 - 5 |
| start_with_revive | Starts the player off with a `Dio's Best Friend`. Functionally equivalent to putting a `Dio's Best Friend` in your `starting_inventory`. | true/false |
| item_pickup_step | The number of item pickups which you are allowed to claim before they become an Archipelago location check. | 0 - 5 |

Using the example YAML above: the Risk of Rain 2 player will have 15 total items which they can pick up for other players (total_locations = 15). 
They will complete a location check every second item (item_pickup_step = 1).
They will have 4 of the items which other players can grant them replaced with `Dio's Best Friend`. (total_revivals = 4).
The player will also start with a `Dio's Best Friend`. (start_with_revive = true)

## Connecting to an Archipelago Server

I assume you already have a server running. Instructions on how to run a server are available on https://archipelago.gg.

There will be a menu button on the right side of the screen. Click it in order to bring up the in lobby mod config. From here you can expand the Archipelago sections and fill in the relevant info.

Keep password blank if there is no password on the server.

![In Lobby UI Example](./docs/img/inlobbyui.png)

Simply check `Enable Archipelago?` and when you start the run it will automatically connect.

## Changelog

**0.1.1**

* Fix victory condition sending for commencement.
* Remove splash+intro cutscene skip (was for debugging purposes).

**0.1.0**

* Initial version.

## Known Issues

* Reconnect logic is unhelpful and may not reconnect you in the event of a network hiccup. You can restart the run but you may not send item checks for a while since
it will also restart the item counter. (Which determines when to send an item check.) Items will still disappear, though, so it's not a perfect situation.
