# Sample code to upload a file:
# r = requests.get('http://127.0.0.1:8080/generate_upload_url')
# requests.post(r['url'], data=r['form'], files={'file': 'abcdefg'})

import random
import os
import datetime
import logging
import tempfile
from flask import Flask
from flask import jsonify
from flask import abort
from google.cloud import storage


APPID = os.environ.get('GOOGLE_CLOUD_PROJECT', 'oni-ruins-test')


app = Flask(__name__)
app.logger.setLevel(logging.INFO)
logging.basicConfig(level=logging.INFO)


def MakeStorageClient():
    account_key_path = tempfile.mktemp()
    storage.Client().bucket(APPID + '.appspot.com').blob('secrets/service-account.json').download_to_filename(account_key_path)
    storage_client = storage.Client.from_service_account_json(account_key_path)
    os.unlink(account_key_path)
    return storage_client


storage_client = MakeStorageClient()
bucket = storage_client.get_bucket(APPID + '.appspot.com')


@app.route('/generate_upload_url')
def generate_upload_url():
    filename = 'upload/%d.yaml.gz' % random.randint(0, 1<<64)
    expiration = datetime.datetime.utcnow() + datetime.timedelta(seconds=10)
    logging.info('Generating upload URL for blob %s', filename)
    policy = bucket.generate_upload_policy(
            expiration=expiration,
            conditions=[
                ['eq', '$key', filename],
                ['content-length-range', 0, 1000000],
                ['eq', '$Content-Type', 'application/gzip']])
    return jsonify({
        'url': 'https://storage.googleapis.com/' + bucket.name,
        'form': {
           'key': filename,
           'GoogleAccessId': '%s@%s.iam.gserviceaccount.com' % (APPID, APPID),
           'policy': policy['policy'],
           'signature': policy['signature'],
           'Content-Type': 'application/gzip'}})


@app.route('/generate_download_url')
def generate_download_url():
    blobs = list(bucket.list_blobs(prefix="ruins/"))
    if not blobs:
        abort(404)
    blob = random.choice(blobs)
    logging.info("Generating download URL for blob %s", blob.name)
    return blob.generate_signed_url(
        expiration=datetime.timedelta(seconds=30), version='v4')


if __name__ == '__main__':
    app.run(host='127.0.0.1', port=8080, debug=True)
