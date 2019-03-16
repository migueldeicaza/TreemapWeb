
flexFont = function () {
    var divs = document.getElementsByClassName("text-wrapper");
    for(var i = 0; i < divs.length; i++) {
        var relFontsize = divs[i].offsetWidth*0.05;
        divs[i].style.fontSize = relFontsize+'px';
    }
};
/*
window.onload = function(event) {
    flexFont();
};
window.onresize = function(event) {
    flexFont();
    };
*/