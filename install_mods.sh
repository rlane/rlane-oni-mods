#!/bin/bash -eux
test -f rlane-oni-mods.sln || exit 1  # Must be run from root of the repository.
DST=~/Documents/Klei/OxygenNotIncluded/mods/dev
for MOD in HeatingElement Alarm MeteorDefenseLaser Endpoint ErrorStackTrace; do
	mkdir -p $DST/$MOD
	cp $MOD/bin/Debug/$MOD.dll $DST/$MOD/
done
