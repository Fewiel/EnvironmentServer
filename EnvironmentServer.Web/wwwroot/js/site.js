﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const CopyToClipboard = (text) => {
    const temp = document.createElement("input");
    temp.setAttribute("value", text);
    document.body.appendChild(temp);
    temp.select();
    document.execCommand("copy");
    document.body.removeChild(temp);
    Swal.fire({
        icon: 'success',
        title: 'Copied to your clipboard',
        showConfirmButton: false,
        timer: 2000
    })
};

function AreYouSurePromt() {
    return new Promise(function (resolve, reject) {
        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, do it!',
            cancelButtonText: 'No, cancel!'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire(
                    'Done!',
                    'There is no more turning back...',
                    'success'
                )
                resolve();
            } else if (
                /* Read more about handling dismissals below */
                result.dismiss === Swal.DismissReason.cancel
            ) {
                Swal.fire(
                    'Cancelled',
                    'Maybe next time...',
                    'error'
                )
                reject();
            }
        });
    })
};