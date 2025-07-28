# Treex
Fancier tree command for Windows with more options and colorized output.

Currently this is a build-it-yourself tool using VS2022 and .NET8.
If you provide an env var called `TOOLS_PATH` and a corresponding entry in `PATH`,
build copies the executables to it. This is also where the application looks for your
specific `treex.ini` file.
Alternatively, the `App.cs` and `Treex.csproj` can be modified to taste.

# Configuration

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
*_files                 | CSV list of color, ext1, ext2, ...                | NA


Some defaults can then be overidden on the command line:

`treex [-f] [-c] [-m N] [-d] [-s] [-i fld 1,fld2,...] [-u fld1,fld2,...] [-?] [dir]`

Option              | Description
-----------         | -----------
dir                 | start folder (default is '.')
-c                  | color output off (for clipboard ops)
-m num              | max depth (0 means all)
-d                  | show_dirs -> true
-s                  | show_size -> true
-e fld1,fld2,...    | exclude directory(s)
-i fld1,fld2,...    | unexclude directory(s)
-?                  | help

