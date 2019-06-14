#!/bin/bash -eux
test -f rlane-oni-mods.sln || exit 1  # Must be run from root of the repository.
SRC="/c/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
DST=oni-libs
mkdir -p "$DST"
cp "$SRC"/*.dll "$DST/"
