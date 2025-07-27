# Treex
Fancier tree command for Windows with more options and colorized output.

Currently this is a build-it-yourself tool using VS2022 and .NET8. It requires an env var called
`TOOLS_PATH` where the binaries are copied, and a corresponding entry in `PATH`.
Alternatively, the `App.cs` and `Treex.csproj` can be modified to taste.

## Configuration

Copy `treex_default.ini` to `$TOOLS_PATH\treex.ini` and edit to taste.

Element                 | Description                                       | Default
-----------             | -----------                                       | ------------------------
show_dirs               | Show dirs only in output                          | false
show_size               | Show file sizes in output                         | false
max_depth               | How deep to look                                  | 3
ascii                   | Use ascii otherwise unicode chars                 | true
exclude_directories     | CSV list of noisy or uninteresting directories    | bin, obj, ...
dir_color               | Color for directories                             | blue
err_color               | Color for syntax and internal errors              | red
*_files                 | CSV list of color, ext1, ext2, ...                |


Some defaults can then be overidden on the command line:

`treex [-f] [-c] [-m N] [-d] [-s] [-i fld 1,fld2,...] [-u fld1,fld2,...] [-?] [dir]`

Option              | Description                           | Default Override
-----------         | -----------                           | ------------------------
dir                 | start folder or current if missing    | NA
-c                  | color output off                      | NA
-m num              | maxDepth (0 means all)                | Y
-d                  | show dirs only                        | Y
-s                  | show size (file only)                 | Y
-e fld1,fld2,...    | exclude directory(s)                  | adds to default exclusions
-i fld1,fld2,...    | unexclude directory(s)                | removes from default exclusions aka include
-?                  | help                                  |

