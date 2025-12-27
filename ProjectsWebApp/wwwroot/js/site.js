var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    $('#tblData').DataTable({
        "ajax": {
            url: '/admin/project/getall',
            type: 'GET',
            datatype: 'json'
        },
        "columns": [
            {
                data: 'mainImageUrl',
                "render": function (data) {
                    return `<img src="${data}" class="project-image" alt="Project Image">`;
                },
                "width": "15%"
            },
            { data: 'title', "width": "50%" },
            { data: 'displayOrder', defaultContent: "N/A", "width": "15%" },
            {
                data: 'isEnabled',
                render: function (data, type, row) {
                    let checked = data ? "checked" : "";
                    return `
                        <div class="form-check form-switch">
                            <input class="form-check-input toggle-status" type="checkbox" ${checked} data-id="${row.id}">
                        </div>`;
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="d-flex justify-content-center">
                            <a href="/admin/project/upsert?id=${data}" class="btn btn-warning mx-2">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                            <a href="/admin/project/delete?id=${data}" class="btn btn-danger mx-2 delete-project">
                                <i class="bi bi-trash-fill"></i>
                            </a>
                        </div>`;
                },
                "width": "20%"
            }
        ],
        "language": {
            "emptyTable": "No data available in table",
            "lengthMenu": "Show _MENU_ entries",
            "info": "Showing _START_ to _END_ of _TOTAL_ entries",
            "infoEmpty": "No entries available",
            "search": "Search:",
            "paginate": {
                "first": "First",
                "last": "Last",
                "next": "Next",
                "previous": "Previous"
            }
        },
        "order": [[1, "asc"]] // Order by title column by default
    });
}

window.onbeforeunload = function () {
    sessionStorage.setItem("lastScrollPosition", window.scrollY);
};

window.onload = function () {
    const lastScrollPosition = sessionStorage.getItem("lastScrollPosition");
    if (lastScrollPosition) {
        window.scrollTo(0, parseInt(lastScrollPosition, 10));
    }
};
