# strava-tools
Tools for interacting with Strava server. Mainly adds ability to modify existing fit files from Strava sessions to add steps.

## Context
Strava, at one point, introduced per-month step challenges. Unfortunately for me, I'm using a Xiaomi Band 6 to record my walks
and even though Xiaomi does have all the data, it produces a fit file which does not have any steps/cadence data. Hence, the 
step counter of the challenge remains at - / #. This tool basically downloads the fit file sent to Strava, by Xiaomi, modifies
it via FIT SDK to include, at the very least, the total steps of the activity itself. Then it deletes the previously created
entry for the activity and uploads the corrected fit file.

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
