# Treex
Fancy tree command for Windows.

## Configuration

Copy `treex_default.ini` to `%APPDATA%\Ephemera\Treex\treex_default.ini` and edit to taste.

Defaults can be overidden on the command line:

`treex [dir] [-f] [-d N] [-s] [-?] [-i fld 1,fld2,...] [-u fld1,fld2,...]`

Option              | Description                       | Default Override
-----------         | -----------                       | ------------------------
dir                 | start folder or '.' if missing    | NA
-d num              | maxDepth (0 means all)            | Y
-f                  | show files                        | Y
-s                  | show size (file only)             | Y
-i fld1,fld2,...    | ignore folders  ()                | adds to default
-u fld1,fld2,...    | unignore folders  ()              | removes from default
-?                  | help                              |

