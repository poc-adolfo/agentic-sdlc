def calcular_desconto(preco, percentual):
    """Calcula o preco final aplicando um desconto percentual."""
    if percentual < 0 or percentual > 100:
        raise ValueError("percentual deve estar entre 0 e 100")
    return preco - (preco * percentual / 100)


def aplicar_cupom(preco, codigo_cupom):
    """Aplica um cupom de desconto fixo baseado no codigo."""
    cupons = {"PROMO10": 10, "PROMO20": 20, "BLACKFRIDAY": 50}
    percentual = cupons.get(codigo_cupom, 0)
    return calcular_desconto(preco, percentual)
