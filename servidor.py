import json
import logging
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


LOGGER = logging.getLogger(__name__)
REABILITATION_PATH = "/health/reabilitacao"
REABILITATION_RESPONSE = {"status": "ok", "origem": "teste-implementar-antiga"}


class HealthRequestHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path != REABILITATION_PATH:
            self.send_error(404)
            return

        payload = json.dumps(REABILITATION_RESPONSE).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)
        LOGGER.info("health_reabilitacao_served", extra={"path": self.path})

    def log_message(self, format, *args):
        LOGGER.info("http_request", extra={"request": format % args})


def create_server(address):
    logging.basicConfig(level=logging.INFO)
    return ThreadingHTTPServer(address, HealthRequestHandler)
