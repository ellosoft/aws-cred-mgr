using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "Spectre1000:Use AnsiConsole instead of System.Console",
    Justification = "Required for non-wrapped text output", Scope = "member",
    Target = "~M:Ellosoft.AwsCredentialsManager.Commands.RDS.GetRdsPassword.GenerateDbPassword" +
             "(System.String,System.String,System.Nullable{System.Int32},System.String,System.String,System.Int32)~System.Threading.Tasks.Task")]
