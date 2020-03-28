# Sample code to upload a file:
# r = requests.get('http://127.0.0.1:8080/generate_upload_url')
# requests.post(r['url'], data=r['form'], files={'file': 'abcdefg'})

import random
from flask import Flask
from flask import jsonify
from google.cloud import storage


app = Flask(__name__)
storage_client = storage.Client()


@app.route('/generate_upload_url')
def generate_upload_url():
    appid = 'oni-ruins-test'
    bucket = storage_client.get_bucket(appid + '.appspot.com')
    filename = 'ruin.%d.yaml' % random.randint(0, 1<<64)
    policy = bucket.generate_upload_policy(
            conditions=[
                ['eq', '$key', filename],
                ['content-length-range', 0, 1000000],
                ['eq', '$Content-Type', 'text/yaml']])
    return jsonify({
        'url': 'https://storage.googleapis.com/' + bucket.name,
        'form': {
           'key': filename,
           'GoogleAccessId': '%s@%s.iam.gserviceaccount.com' % (appid, appid),
           'policy': policy['policy'],
           'signature': policy['signature'],
           'Content-Type': 'text/yaml'}})

if __name__ == '__main__':
    app.run(host='127.0.0.1', port=8080, debug=True)
