A small Windows utility to sync save files in DRG between Steam and Xbox/Microsoft platforms.

It syncs your save files by comparing the save files for both versions of the game, then copying and renaming the most recent save to the other platform's savegame directory.
Every time a sync is performed, a backup of both versions is made, so you can always restore them, if needed.

**Who does this benefit?**
Anyone who owns the game on both the Xbox and Steam platforms and wants to maintain their game progress across both platforms.

**Why do some people own the game on separate platforms?**
The Steam release has some benefits including quicker updates, a larger online player base, mod-support, and, reportedly, improved cave generation since the Xbox release is intended to support older consoles.
However, the Steam release does not support cross-play with Xbox users. So, if you have a friend that is on Xbox, you need to buy and use a separate copy of the game from the Microsoft/Xbox store.
Unfortunately the separate releases track progress separately.

Luckily, there is a way to transfer your game progress between the platforms and the method has been deemed "OK" by the game's developer.
This method, while not complicated, is tedius when performed manually, especially if you switch between platforms often. 
It can also be a pain if you forget to transfer and then are forced to overwrite your newer save file with your main save, losing any progress made in the meantime.

**Why did you make this?**
I found myself in the situation described above and wanted to avoid performing the manual sync for both myself and my girlfriend everytime we wanted to play with our friend (on Xbox).
I initially considered building this in Python, but decided it would be a good opportunity to learn about building applications for Windows, and it was a fun couple of hours.
