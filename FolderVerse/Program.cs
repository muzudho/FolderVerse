using var game = new FolderVerse.Game1 { FitWindowOnStartup=true };
try{game.Run();}
catch(System.Exception error){game.RecordFailure(error,"game_run");throw;}
