/*
 *  Check Traders
 */
function displayhelper() {
    const elements = document.getElementsByClassName("trader");

    if (elements.length != null) {
        for (let i = 0; i < elements.length; i++) {
            elements[i].removeClass = "bg-primary";
            elements[i].addClass = "bg-success";
        }
    }
}
