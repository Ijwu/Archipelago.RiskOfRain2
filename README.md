# Archipelago.RiskOfRain2

This mod adds support to Risk of Rain 2 for playing as an Archipelago client. For more information on Archipelago head over to https://archipelago.gg.

Should be multiplayer compatible, but not rigorously tested. Be sure to scale up your YAML settings if you play in multiplayer. Only the host needs the mod.

## Gameplay 

The Risk of Rain 2 players send checks by causing items to spawn in-game. That means opening chests or killing bosses, generally. 
An item check is only sent out after a certain number of items are picked up. This count is configurable in the player's YAML.

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

## Changelog

**0.1.0**

* Initial version.
