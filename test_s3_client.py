from s3_client import obter_cliente_s3


def test_obter_cliente_s3_retorna_algo():
    cliente = obter_cliente_s3()
    assert cliente is not None
