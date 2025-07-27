# Treex
Fancier tree command for Windows with more options and colorized output.

Currently this is a build-it-yourself tool using VS2022 and .NET8. It requires an env var called
`TOOLS_PATH` where the binaries are copied, and a corresponding entry in `PATH`.
Alternatively, the `App.cs` and `Treex.csproj` can be modified to taste.

## Configuration

Copy `treex_default.ini` to `$TOOLS_PATH\treex.ini` and edit to taste.

Element                 | Description                                       | Default
-----------             | -----------                                       | ------------------------
show_files              | Show files in output                              | true
show_size               | Show file sizes in output                         | false
max_depth               | How deep to look                                  | 3
ascii                   | Use ascii otherwise unicode chars                 | true
exclude_directories     | CSV list of noisy or uninteresting directories    | bin, obj, ...
image_files             | CSV list                                          | For colorizing
audio_files             | CSV list                                          | For colorizing
binary_files            | CSV list                                          | For colorizing
executable_files        | CSV list                                          | For colorizing
dir_color               | Color for directories                             | blue
file_color              | Color for files                                   | none
exe_color               | Color for executables                             | yellow
bin_color               | Color other binaries                              | grreen
err_color               | Color for syntax and internal errors              | red


Some defaults can then be overidden on the command line:

`treex [-f] [-d N] [-s] [-?] [-i fld 1,fld2,...] [-u fld1,fld2,...] [dir]`

Option              | Description                           | Default Override
-----------         | -----------                           | ------------------------
dir                 | start folder or current if missing    | NA
-c                  | color output off                      | NA
-d num              | maxDepth (0 means all)                | Y
-f                  | show files                            | Y
-s                  | show size (file only)                 | Y
-e fld1,fld2,...    | exclude directory(s)                  | adds to default exclusions
-i fld1,fld2,...    | unexclude directory(s)                | removes from default exclusions aka include
-?                  | help                                  |

