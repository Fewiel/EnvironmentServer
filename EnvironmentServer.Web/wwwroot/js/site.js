// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const CopyToClipboard = (text) => {
    const temp = document.createElement("input");    
    temp.setAttribute("value", text);
    document.body.appendChild(temp);
    temp.select();
    document.execCommand("copy");
    document.body.removeChild(temp);
    alert("Password copied to your clipboard!");
};