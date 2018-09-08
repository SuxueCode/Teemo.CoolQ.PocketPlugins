param($installPath, $toolsPath, $package, $project)

foreach($dir in $project.ProjectItems.Item("Newbe.CQP.Framework").ProjectItems){
	foreach($file in $dir.ProjectItems){
		$configItem = $file
		# set 'Build Action' to 'None'
		$buildAction = $configItem.Properties.Item("BuildAction")
		$buildAction.Value = 0
	}
}
