// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function startTime() {
    var today = new Date();
    var h = today.getHours();
    var m = today.getMinutes();
    var s = today.getSeconds();
    m = checkTime(m);
    s = checkTime(s);
    document.getElementById('lblLiveTime').innerHTML = today.toString();
    //+ " " +  h + ":" + m + ":" + s;
    var t = setTimeout(startTime, 500);
}

function checkTime(i) {
    if (i < 10) { i = "0" + i };  // add zero in front of numbers < 10
    return i;
}

function GetTemplates(groupId, templateId) {
    var opt = '<option value="">Select Template</option>';

    $.getJSON('/reservations/gettemplates/', {groupId: groupId}, function (data) {
        
        for (var i = 0; i < data.length; i++) {
            if (templateId == data[i].value) {
                opt += '<option selected="selected" value="' + data[i].value + '">' + data[i].text + '</option>';
            }
            else {
                opt += '<option value="' + data[i].value + '">' + data[i].text + '</option>';
            }
        }


        $('#TemplateId').html(opt);
    })
}

function AddHourInDate(d, h) {
    var dayChanged = false;
    var monthChanged = false;
    var yearChanged = false;

    var newHr = d.getHours() + h;
    if (newHr < 10)
        newHr = '0' + newHr;
    else if (newHr > 24) {
        dayChanged = true;
        newHr = '0' + (newHr - 24);
    }

    var newDate = d.getDate();
    if (dayChanged === true)
        newDate = newDate + 1;

    if (newDate < 10)
        newDate = '0' + newDate;
    else if (newDate > new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate()) {
        newDate = '0' + (newDate - new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate());
        monthChanged = true;
    }

    var newMon = d.getMonth() + 1;
    if (monthChanged === true)
        newMon = newMon + 1;

    if(newMon > 12) {
        newMon = newMon - 12;
    }

    if (newMon < 10)
        newMon = '0' + newMon;
   
    var newMin = d.getMinutes();
    if (newMin < 10)
        newMin = '0' + newMin;

    return d.getFullYear() + '-' + newMon + '-' + newDate + 'T' + newHr + ':' + newMin;

}

$(function () {
    var current = location.pathname;
    if (current != '/') {
        $('#header').addClass('header-inner-pages');

        console.log(current);

        $('#nav li a').each(function () {
            var $this = $(this);
            // if the current path is like this link, make it active
            if ($this.attr('href').indexOf(current) !== -1) {
                $this.addClass('active');
            }
        })
    }

    
})
