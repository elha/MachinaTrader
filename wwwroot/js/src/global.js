// Wire up signalr and js intervals -> preparePage() is hooked from navigation script on page change
/* eslint-disable */

const jsInterval = {}
const signalrConnections = {}

function runJsInterval(script, str, delay) {
  if (!jsInterval[str]) {
    // console.log("Not in array")
    jsInterval[str] = setInterval(script, delay)
  }
}

function clearJsInterval() {
  for (const key in jsInterval) {
    if (Object.prototype.hasOwnProperty.call(jsInterval, key)) {
      clearInterval(jsInterval[key])
      jsInterval[key] = null
      delete jsInterval[key]
    }
  }
}

/* eslint-disable no-unused-vars */
function addSignalrClient(name, connection) {
  if (!signalrConnections[name]) {
    // console.log("Not in array")
    signalrConnections[name] = connection
  }
}

function cleanSignalr() {
  for (const key in signalrConnections) {
    if (Object.prototype.hasOwnProperty.call(signalrConnections, key)) {
      // console.log("Disconnect")
      // console.log(signalrConnections[key])
      signalrConnections[key].stop()
      delete signalrConnections[key]
    }
  }
}

function beforeHook() {
	// Hacky way to remove intervals
	/*for(i=0; i<100; i++)
	{
		window.clearInterval(i);
	}*/		
	//Cleanup all keydown listeners -> Needed for some JS components
	$(document).off("keydown");	
	clearJsInterval();
	cleanSignalr();
}

function afterHook() {
	$(".select2").select2({
		theme: "bootstrap",
		templateResult: selectPickerImage,
		templateSelection: selectPickerImage,
		width: "100%"
	});
}