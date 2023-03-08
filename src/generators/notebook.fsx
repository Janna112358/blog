#r "../_lib/Fornax.Core.dll"
#load "../globals.fsx"
#load "layout.fsx"
#if !FORNAX
#load "../loaders/postloader.fsx"
#endif

open Postloader
open Layout
open Html

open System.IO
open System.Diagnostics

let processConvertedNotebook (lang:PostLanguage) (content:string) =
    let nb_start_tag = """<body class="jp-Notebook" data-jp-theme-light="true" data-jp-theme-name="JupyterLab Light">"""
    let body_start_index = content.IndexOf(nb_start_tag) + nb_start_tag.Length
    let body_end_index = content.IndexOf "</body>" + 1
    let hl_class = PostLanguage.toHighlightClass lang
    content[body_start_index .. body_end_index].Replace(" highlight hl-ipython3",hl_class)



let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let full_path = Path.Combine(projectRoot,page)
    let tmp_path = Path.GetTempPath ()
    let tmp_output = Path.GetFileName(full_path).Replace("ipynb","html")
    let output_path = Path.Combine(tmp_path, tmp_output)

    let lang = 
        ctx.TryGetValues<NotebookPost>()
        |> Option.map (fun x -> 
            x 
            |> Seq.tryFind (fun n -> 
                n.original_path.Replace("\\","/") = full_path.Replace("\\","/")) 
            |> Option.map (fun x -> x.post_config.language)
            |> Option.defaultValue PostLanguage.Other
        ) |> Option.defaultValue PostLanguage.Other

    printfn $"starting jupyter --output-dir='{tmp_path}' nbconvert --to html {full_path}"
    let psi = ProcessStartInfo()
    psi.FileName <- "jupyter"
    psi.Arguments <- $"nbconvert --output-dir='{tmp_path}' --to html {full_path}"
    psi.CreateNoWindow <- true
    psi.WindowStyle <- ProcessWindowStyle.Hidden
    psi.UseShellExecute <- true
    try
        let proc = Process.Start psi
        proc.WaitForExit()
        let notebook_content = File.ReadAllText output_path
        File.Delete output_path

        Layout.layout ctx "Posts" [
            div [
                Class "content jp-Notebook"
                HtmlProperties.Custom ("data-jp-theme-light","true")
                HtmlProperties.Custom ("data-jp-theme-name","JupyterLab Light")
            ] [!!(processConvertedNotebook lang notebook_content)]
        ]
        |> Layout.render ctx
    with
    | ex ->
        printfn "EX: %s" ex.Message
        printfn "make sure to set up a conda distribution with nbconvert installed."
        Layout.layout ctx "" List.empty
        |> Layout.render ctx