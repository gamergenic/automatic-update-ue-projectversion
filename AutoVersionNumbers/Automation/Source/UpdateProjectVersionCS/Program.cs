// Copyright 2023 Gamergenic.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Implementation walkthrough:
// "Automagically Incrementing UE App Version Numbers"
// https://gamedevtricks.com/post/automagically-updating-ue-app-version/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Perforce.P4;

namespace UpdateProjectVersionCS
{
    [Serializable]
    public struct P4Credentials
    {
        public string ServerUri { get; set; }
        public string P4User { get; set; }
        public string P4Password { get; set; }
        public string P4Client { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("Usage UpdateProjectVersionCS.exe <projectdir>\n");
                return;
            }

            // It is bad practice to try/catch everything, but doing so for clarity
            try
            {
                // Figure out where everything is.
                string UnrealProjectDirectory = args[0];
                string PathToDefaultGameINI = Path.Combine(UnrealProjectDirectory, "Config\\DefaultGame.ini");
                string PathToP4Credentials = Path.Combine(UnrealProjectDirectory, "Automation\\P4Credentials.json");

                bool bNeedToSubmit = false;
                P4Credentials P4Credentials = default;

                //
                // First - prepare the file.  If perforce is active, sync it and check out the file for edits.
                //
                Connection con = null;

                // Assume we need to connect to p4 if the credentials file exists, and do not, if it does not
                if (System.IO.File.Exists(PathToP4Credentials))
                {
                    string CredentialsJSON = System.IO.File.ReadAllText(PathToP4Credentials);

                    P4Credentials = JsonSerializer.Deserialize<P4Credentials>(CredentialsJSON);

                    // Assume this means we'll need to submit the update.

                    string uri = P4Credentials.ServerUri;
                    string user = P4Credentials.P4User;
                    string ws_client = P4Credentials.P4Client;
                    string pass = P4Credentials.P4Password;

                    // define the server, repository and connection
                    Server server = new Server(new ServerAddress(uri));
                    Repository rep = new Repository(server);
                    con = rep.Connection;

                    // use the connection variables for this connection
                    con.UserName = user;
                    con.Client = new Client();
                    con.Client.Name = ws_client;

                    // connect to the server
                    con.Connect(null);

                    Options LoginOptions = new Options();

                    Credential cred = con.Login(pass, LoginOptions, user);

                    FileSpec DefaultGameFileSpec = new FileSpec(null, null, new LocalPath(PathToDefaultGameINI), null);

                    // First, ensure the local file is sync'ed
                    Options SyncOptions = new SyncFilesCmdOptions(SyncFilesCmdFlags.None);
                    con.Client.SyncFiles(SyncOptions, DefaultGameFileSpec);

                    // At this point, you really should resolve.  There may have been local edits!

                    Options EditOptions = new Options();

                    con.Client.EditFiles(EditOptions, DefaultGameFileSpec);

                    bNeedToSubmit = true;
                }

                //
                // Edit the INI file
                //

                string[] FileLines = System.IO.File.ReadAllLines(PathToDefaultGameINI);
                bool bFileDirty = false;

                for(int i = 0; i < FileLines.Length; ++i)
                {
                    string Line = FileLines[i];

                    if(Line.Trim().StartsWith("ProjectVersion="))
                    {
                        string[] VersionNumbers = Line.Split('.');

                        if(VersionNumbers.Length > 0)
                        {
                            string BuildNumberString = VersionNumbers[VersionNumbers.Length - 1];

                            long BuildNumber;
                            if(Int64.TryParse(BuildNumberString, out BuildNumber))
                            {
                                BuildNumber++;
                                string EditedLine = FileLines[i].Substring(0, FileLines[i].LastIndexOf('.')+1);
                                EditedLine += BuildNumber.ToString();
                                FileLines[i] = EditedLine;
                                bFileDirty = true;
                                break;
                            }
                            else
                            {
                                throw new System.Exception("Failed to parse build number");
                            }
                        }
                        else
                        {
                            throw new Exception("Count not find build number");
                        }
                    }
                }

                if(bFileDirty)
                {
                    System.IO.File.WriteAllLines(PathToDefaultGameINI, FileLines);
                }

                // What if the project version wasn't found?   Really should add it.
                // It must go under "[/Script/EngineSettings.GeneralProjectSettings]"
                // Omitted for clarity

                //
                // Submit the updated file to perforce
                //

                if(bNeedToSubmit)
                {
                    SubmitCmdOptions SubmitOptions = new SubmitCmdOptions(SubmitFilesCmdFlags.None, 0, null, "AUTOMATION: Update Project Version", null);
                    FileSpec DefaultGameFileSpec = new FileSpec(null, null, new LocalPath(PathToDefaultGameINI), null);

                    con.Client.SubmitFiles(SubmitOptions, DefaultGameFileSpec);
                }

            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error updating Project Version:");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
