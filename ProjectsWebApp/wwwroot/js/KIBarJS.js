let dataTable1; // ✅ declare it globally
const BASE_PATH = window.BASE_PATH || '/';
const CSRF_TOKEN =
    document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
$(document).ready(function () {
    dataTable1 = $('#tblMakerSpace').DataTable({ // ✅ assign it here
      
        "ajax": {
            url: BASE_PATH + 'admin/makerspaceproject/getall',
            type: 'GET',
            datatype: 'json'
        },
        "columns": [
            { data: 'verlauf', width: "3%" },
            {
                data: 'imageUrl',
                render: function (data) {
                    return `
    <img src="${data}" style="width: 100px; height: 60px; object-fit: contain; background-color: #f9f9f9; border-radius: 6px;" />`;
                },
                width: "10%"
            },

            { data: 'title', width: "20%" },

            {
                data: 'lesezeichen',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input lesezeichen-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },

            {
                data: 'forschung',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input forschung-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },

            {
                data: 'top',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input top-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },

            {
                data: 'events',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input events-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },

            {
                data: 'tutorial',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input tutorial-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },
            {
                data: 'itRecht',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input ITRecht-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },
            {
                data: 'beitraege',
                render: function (data, type, row) {
                    const checked = data ? 'checked' : '';
                    return `<div class="form-check form-switch text-center">
        <input class="form-check-input beitraege-toggle" type="checkbox" data-id="${row.id}" ${checked}>
    </div>`;
                },
                width: "8%"
            },



            {
                data: 'id',
                render: function (data) { // ✅ Single render function
                    return `
    <div class="d-flex justify-content-center">
        <a href="/promptcoach/admin/makerspaceproject/upsert?id=${data}" class="btn btn-warning mx-2">
            <i class="bi bi-pencil-square"></i>
        </a>
        <button class="btn btn-danger mx-2 delete-project" data-id="${data}">
            <i class="bi bi-trash-fill"></i>
        </button>
    </div>`;
                },
                width: "10%"
            }
        ],

        "language": {
            "emptyTable": "Keine Daten verfügbar",
            "search": "Suchen:",
            "lengthMenu": "Zeige _MENU_ Einträge",
            "info": "Zeige _START_ bis _END_ von _TOTAL_ Einträgen",
            "paginate": {
                "next": "Nächste",
                "previous": "Vorherige"
            }
        },
        
        "order": [[0, "asc"]],
        "pageLength": 50,
        "stateSave": true,
        initComplete: function () {
            this.api().page.len(50).draw(false);
        }
    });


    $(document).on('change', '.top-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url:  '/promptcoach/Admin/MakerSpaceProject/ToggleTop',
            type: 'POST',
            data: { id: id, isTop: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.forschung-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleForschung',
            type: 'POST',
            data: { id: id, isForschung: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.download-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleDownload',
            type: 'POST',
            data: { id: id, isDownload: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.tutorial-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url:  '/promptcoach/Admin/MakerSpaceProject/ToggleTutorial',
            type: 'POST',
            data: { id: id, isTutorial: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.netzwerk-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleNetzwerk',
            type: 'POST',
            data: { id: id, isNetzwerk: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });


    $(document).on('change', '.events-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url:  '/promptcoach/Admin/MakerSpaceProject/ToggleEvent',
            type: 'POST',
            data: { id: id, isEvent: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.lesezeichen-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleLesezeichen',
            type: 'POST',
            data: { id: id, isLesezeichen: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    $(document).on('change', '.ITRecht-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleITRecht',
            type: 'POST',
            data: { id: id, isITRecht: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });


    $(document).on('change', '.beitraege-toggle', function () {
        const id = $(this).data('id');
        const isChecked = $(this).is(':checked');

        $.ajax({
            url: '/promptcoach/Admin/MakerSpaceProject/ToggleBeitraege',
            type: 'POST',
            data: { id: id, isBeitraege: isChecked },
            success: function (response) {
                if (!response.success) {
                    Swal.fire('Fehler!', response.message, 'error');
                }
            },
            error: function () {
                Swal.fire('Fehler!', 'Ein Fehler ist aufgetreten.', 'error');
            }
        });
    });

    // DELETE‑Handler
    $(document).on('click', '.delete-project', function () {
        const id = $(this).data('id');

        Swal.fire({
            title: 'Projekt löschen?',
            text: 'Sind Sie sicher, dass Sie dieses Projekt unwiderruflich löschen möchten?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Ja, löschen!',
            cancelButtonText: 'Abbrechen'
        }).then(result => {
            if (!result.isConfirmed) return;

            /* ---------- HIER: neuen Aufruf einsetzen ---------- */
            $.ajax({
                url: BASE_PATH + 'admin/makerspaceproject/delete',   // → arbeitet lokal & online
                type: 'POST',
                data: {
                    id,
                    __RequestVerificationToken: CSRF_TOKEN             // Anti‑Forgery‑Token mitsenden
                },
                success: resp => {
                    if (resp.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Gelöscht!',
                            timer: 1800,
                            showConfirmButton: false
                        }).then(() => dataTable1.ajax.reload(null, false));   // Tabelle neu laden
                    } else {
                        Swal.fire('Fehler!', resp.message, 'error');
                    }
                },
                error: xhr => {
                    Swal.fire('Fehler!', xhr.responseText || 'Unbekannter Fehler', 'error');
                }
            });
            /* --------------------------------------------------- */
        });
    });


});