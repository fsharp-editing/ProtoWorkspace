module MSBuildProjectSystem

open Microsoft.Extensions.Configuration

type IProjectSystem =
    abstract Key : string
    abstract Language : string
    abstract Extensions : string seq
    abstract Initialize : Configuration:IConfiguration -> unit



// NOTE - Maybe use the msbuildprojectloader for this?
