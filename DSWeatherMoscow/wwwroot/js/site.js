document.addEventListener("DOMContentLoaded", function () {
    var form = document.querySelector("form");
    var uploadButton = document.getElementById("uploadButton");

    form.addEventListener("submit", function () {
        // Disable the upload button on form submission
        uploadButton.disabled = true;
        uploadButton.textContent = 'Uploading...'; // Optional: Change button text
    });
});

