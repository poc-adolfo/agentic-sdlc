import json
import threading
import unittest
from http.client import HTTPConnection

from servidor import create_server


class TestRotaImplementarReabilitada(unittest.TestCase):
    def test_health_reabilitacao_retorna_status_e_origem(self):
        server = create_server(("127.0.0.1", 0))
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()

        try:
            connection = HTTPConnection("127.0.0.1", server.server_port)
            connection.request("GET", "/health/reabilitacao")
            response = connection.getresponse()

            self.assertEqual(response.status, 200)
            self.assertEqual(
                json.loads(response.read()),
                {"status": "ok", "origem": "teste-implementar-antiga"},
            )
        finally:
            server.shutdown()
            server.server_close()
            thread.join()


if __name__ == "__main__":
    unittest.main()
