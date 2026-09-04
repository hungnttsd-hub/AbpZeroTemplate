Option Explicit

Dim shell, fileSystem, scriptDirectory, nodePath, entryPoint, command, exitCode

If WScript.Arguments.Count <> 1 Then
  WScript.Quit 2
End If

Set shell = CreateObject("WScript.Shell")
Set fileSystem = CreateObject("Scripting.FileSystemObject")

scriptDirectory = fileSystem.GetParentFolderName(WScript.ScriptFullName)
nodePath = WScript.Arguments(0)
entryPoint = fileSystem.BuildPath(scriptDirectory, "index.js")

If Not fileSystem.FileExists(nodePath) Or Not fileSystem.FileExists(entryPoint) Then
  WScript.Quit 2
End If

shell.CurrentDirectory = scriptDirectory
command = Quote(nodePath) & " " & Quote(entryPoint)
exitCode = shell.Run(command, 0, True)

WScript.Quit exitCode

Function Quote(value)
  Quote = Chr(34) & value & Chr(34)
End Function
