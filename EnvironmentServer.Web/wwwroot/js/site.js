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
    Swal.fire({
        icon: 'success',
        title: 'Copied to your clipboard',
        showConfirmButton: false,
        timer: 2000
    })
};

const AreYouSurePromt = Swal.mixin({
    customClass: {
        confirmButton: 'btn btn-success',
        cancelButton: 'btn btn-danger'
    },
    buttonsStyling: false
})

AreYouSurePromt.fire({
    title: 'Are you sure?',
    text: "You won't be able to revert this!",
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, just do it!',
    cancelButtonText: 'No, cancel!',
    reverseButtons: true
}).then((result) => {
    if (result.isConfirmed) {
        AreYouSurePromt.fire(
            'Deleted!',
            'No more turning back...',
            'success'
        )
    } else if (
        /* Read more about handling dismissals below */
        result.dismiss === Swal.DismissReason.cancel
    ) {
        AreYouSurePromt.fire(
            'Cancelled',
            'Maybe next time...',
            'error'
        )
    }
})