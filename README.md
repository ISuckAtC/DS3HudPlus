This project is extremely WIP.

Raylib core is used for drawing to the screen, the screen is transparent and always on top.




TODO:
* Get ortographic images of each map and render them in the minimap window (probably done by loading the whole image per map and masking it to a circle area)
* Add additional information about your own and other player characters
    * Such as HP/SP/FP for your own character
    * Active effects for your own character
    * Equipment (armor/weapon/rings) for other players
    * Current active effects for other players
* Most of the work for the additional info is limited by the chore of having to grab all the icons and mapping them all to the right IDs, actually reading the IDs from another player is trivial.