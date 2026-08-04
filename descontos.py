"""Cálculo de descontos e aplicação de cupons promocionais no checkout.

Este módulo implementa as funções descritas na issue #10:
- `calcular_desconto(preco, percentual)`: aplica um desconto percentual
  ao preço, validando que o percentual esteja no intervalo [0, 100].
- `aplicar_cupom(preco, codigo_cupom)`: aplica o desconto associado a um
  cupom promocional, reaproveitando `calcular_desconto`.

Tabela de cupons suportados:
    PROMO10     -> 10%
    PROMO20     -> 20%
    BLACKFRIDAY -> 50%

Cupons inexentes/inválidos não aplicam desconto (comportamento seguro
por padrão), retornando o preço original inalterado.
"""

from __future__ import annotations

# 1.1 (parcial) — Tabela de cupons promocionais.
# Mapeia código de cupom -> percentual de desconto.
CUPONS: dict[str, int] = {
    "PROMO10": 10,
    "PROMO20": 20,
    "BLACKFRIDAY": 50,
}


def _validar_numero(valor, nome: str) -> None:
    """Garante que `valor` é numérico e não booleano."""
    # `bool` é subclasse de `int`, então precisa ser excluído explicitamente.
    if isinstance(valor, bool) or not isinstance(valor, (int, float)):
        raise TypeError(f"{nome} deve ser numérico (int ou float)")


def calcular_desconto(preco: float, percentual: float) -> float:
    """Calcula o preço final após aplicar um desconto percentual.

    1.1 / 2.1 — Assinatura e implementação com validação de intervalo.

    Args:
        preco: Preço original (>= 0).
        percentual: Percentual de desconto, entre 0 e 100 (inclusive).

    Returns:
        Preço final com o desconto aplicado, arredondado para 2 casas
        decimais (padrão monetário).

    Raises:
        TypeError: Se `preco` ou `percentual` não forem numéricos.
        ValueError: Se `preco` for negativo ou `percentual` estiver fora
            do intervalo [0, 100].
    """
    _validar_numero(preco, "preco")
    _validar_numero(percentual, "percentual")

    if preco < 0:
        raise ValueError("preco não pode ser negativo")
    if percentual < 0 or percentual > 100:
        raise ValueError(
            "percentual deve estar no intervalo [0, 100]; "
            f"recebido: {percentual}"
        )

    desconto = preco * (percentual / 100.0)
    return round(preco - desconto, 2)


def aplicar_cupom(preco: float, codigo_cupom: str) -> float:
    """Aplica o desconto de um cupom promocional ao preço.

    1.2 / 2.2 — Tabela de cupons + assinatura, reaproveitando
    `calcular_desconto`.

    Comportamento seguro por padrão: cupons inexistentes/inválidos NÃO
    aplicam desconto, retornando o preço original.

    Args:
        preco: Preço original (>= 0).
        codigo_cupom: Código do cupom (ex.: "PROMO10").

    Returns:
        Preço final após o desconto do cupom, ou o preço original se o
        cupom não existir.
    """
    _validar_numero(preco, "preco")
    if preco < 0:
        raise ValueError("preco não pode ser negativo")

    percentual = CUPONS.get(codigo_cupom)
    if percentual is None:
        # Cupom inválido/inexistente: nenhum desconto aplicado.
        return float(preco)

    return calcular_desconto(preco, percentual)
