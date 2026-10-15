define([], function () {
    'use strict';
    return function (view) {
        var previewKey = null;
        function el(id) { return view.querySelector('#' + id); }
        function call(path, data, method) {
            return ApiClient.ajax({ type: method || 'POST', url: ApiClient.getUrl('edl_chapters/' + path),
                dataType: 'json', contentType: 'application/json', data: data ? JSON.stringify(data) : undefined });
        }
        function input() {
            return { ItemId: Number(el('edlItem').value), LocalMediaPath: el('edlPath').value.trim() };
        }
        function invalidate() { previewKey = null; el('edlApply').disabled = true; }
        function error(e) { el('edlStatus').textContent = 'Request failed: ' + (e.responseText || e.message || 'Check Emby server log.'); invalidate(); }
        function show(result) {
            el('edlStatus').textContent = result.Status + ': ' + result.Name + ' — ' + result.Breaks + ' break(s).' + (result.ThumbnailExtractionRequested ? ' Thumbnail extraction requested.' : '');
            el('edlOutput').textContent = 'EDL: ' + result.EdlPath + '\nOrdinary chapter points replaced: ' + result.RemovedOrdinaryChapters + '\n\n' +
                result.Chapters.map(function (c) { return (c.StartPositionTicks / 10000000).toFixed(3) + ' seconds  ' + c.Name; }).join('\n');
        }
        el('edlSave').addEventListener('click', function () {
            invalidate();
            call('settings', {ExtractThumbnailsAfterImport: el('edlThumbnails').checked, Root: el('edlRoot').value.trim(), Automatic: el('edlAutomatic').checked})
                .then(function () { el('edlStatus').textContent = 'Import settings saved.'; }).catch(error);
        });
        el('edlPreview').addEventListener('click', function () {
            invalidate(); var data = input();
            if (!Number.isSafeInteger(data.ItemId) || data.ItemId <= 0) { el('edlStatus').textContent = 'Enter a valid item ID.'; return; }
            el('edlStatus').textContent = 'Reading EDL…';
            call('import', data).then(function (result) { show(result); previewKey = JSON.stringify(data); el('edlApply').disabled = result.Status === 'Already imported'; }).catch(error);
        });
        el('edlApply').addEventListener('click', function () {
            var data = input(); if (JSON.stringify(data) !== previewKey) { invalidate(); return; }
            invalidate(); data.Apply = true; el('edlStatus').textContent = 'Importing…';
            call('import', data).then(show).catch(error);
        });
        ['edlItem','edlPath','edlRoot'].forEach(function (id) { el(id).addEventListener('input', invalidate); });
        view.addEventListener('viewshow', function () {
            invalidate();
            call('settings', null, 'GET').then(function (c) { el('edlThumbnails').checked=!!c.ExtractThumbnailsAfterImport; el('edlRoot').value=c.Root;  el('edlAutomatic').checked=c.Automatic; }).catch(error);
        });
    };
});
