# strava-tools
Tools for interacting with Strava server. Mainly adds ability to modify existing fit files from Strava sessions to add steps.

## Usage
```
strava-tools [command] [subcommand] [options]
  
Options:
	-?, -h, --help  Show help and usage information
	--version       Show version information

Commands:
	upload <filepath>						Uploads an Activity .fit file to the server
	delete <activity-id>                	Deletes an activity on the server, given its id
	dump <filepath>                     	Dumps the contents of a local file
	download <activity-id>              	Downloads an Activity .fit file from the server for activities either by id or timeframe
	fix:                                	Fixes an activity's lack of steps, provided the step count
		fix local <filepath> <steps>      	Fixes an activity .fit file locally
	    fix remote <activity-id> <steps>  	Fixes an activity straight from the server, using an activity id
	list-activities                     	Lists the activities on the server including some relevant info
```
