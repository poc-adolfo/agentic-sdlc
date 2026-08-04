"""Testes unitários para descontos e cupons promocionais (issue #10).

Cobre a WBS 3.1 (calcular_desconto) e 3.2 (aplicar_cupom):
- 3.1: casos válidos, limites (0% e 100%) e inválidos (fora do intervalo,
  tipos errados, preço negativo).
- 3.2: cupom válido, cupom inválido (sem desconto) e preço zero.
"""

from __future__ import annotations

import pytest

from descontos import CUPONS, aplicar_cupom, calcular_desconto


# ---------------------------------------------------------------------------
# 3.1 — calcular_desconto
# ---------------------------------------------------------------------------


class TestCalcularDesconto:
    """Testes unitários de `calcular_desconto`."""

    @pytest.mark.parametrize(
        "preco, percentual, esperado",
        [
            (100.00, 10, 90.00),
            (100.00, 25, 75.00),
            (200.00, 50, 100.00),
            (50.00, 0, 50.00),       # 0% -> sem desconto
            (99.99, 100, 0.0),       # 100% -> grátis
            (0.0, 50, 0.0),          # preço zero -> zero
            (37.50, 15, 31.88),      # arredondamento (31.875 -> 31.88)
        ],
    )
    def test_casos_validos(self, preco, percentual, esperado):
        assert calcular_desconto(preco, percentual) == esperado

    @pytest.mark.parametrize("percentual_invalido", [-1, 101, -0.01, 100.01])
    def test_percentual_fora_do_intervalo_rejeita(self, percentual_invalido):
        # Critério de aceite: percentual fora de 0-100 -> erro claro.
        with pytest.raises(ValueError, match="intervalo"):
            calcular_desconto(100.00, percentual_invalido)

    @pytest.mark.parametrize("percentual_limite", [0, 100])
    def test_limites_validos(self, percentual_limite):
        # 0 e 100 são inclusivos e válidos.
        resultado = calcular_desconto(100.00, percentual_limite)
        assert resultado == pytest.approx(100.00 - 100.00 * percentual_limite / 100)

    def test_preco_negativo_rejeita(self):
        with pytest.raises(ValueError, match="negativo"):
            calcular_desconto(-10.00, 10)

    @pytest.mark.parametrize("preco, percentual", [("100", 10), (100, "10"), (None, 10)])
    def test_tipos_nao_numericos_rejeita(self, preco, percentual):
        with pytest.raises(TypeError):
            calcular_desconto(preco, percentual)

    def test_booleano_nao_e_aceito_como_numero(self):
        # `bool` é subclasse de `int`; não deve ser aceito silenciosamente.
        with pytest.raises(TypeError):
            calcular_desconto(True, 10)
        with pytest.raises(TypeError):
            calcular_desconto(100.00, False)


# ---------------------------------------------------------------------------
# 3.2 — aplicar_cupom
# ---------------------------------------------------------------------------


class TestAplicarCupom:
    """Testes unitários de `aplicar_cupom`."""

    @pytest.mark.parametrize("codigo, percentual_esperado", list(CUPONS.items()))
    def test_cupom_valido_aplica_desconto_correspondente(self, codigo, percentual_esperado):
        # Critério de aceite: cupom válido -> desconto correspondente aplicado.
        preco = 200.00
        esperado = round(preco - preco * percentual_esperado / 100.0, 2)
        assert aplicar_cupom(preco, codigo) == esperado

    def test_cupons_especificos_valores(self):
        assert aplicar_cupom(100.00, "PROMO10") == 90.00
        assert aplicar_cupom(100.00, "PROMO20") == 80.00
        assert aplicar_cupom(100.00, "BLACKFRIDAY") == 50.00

    @pytest.mark.parametrize(
        "codigo_invalido", ["", "PROMO15", "promo10", "XPTO", "PROMO10 ", None]
    )
    def test_cupom_invalido_nao_aplica_desconto(self, codigo_invalido):
        # Critério de aceite: cupom inválido/inexistente -> sem desconto.
        preco = 123.45
        assert aplicar_cupom(preco, codigo_invalido) == preco

    def test_preco_zero_com_cupom_valido(self):
        assert aplicar_cupom(0.0, "PROMO10") == 0.0
        assert aplicar_cupom(0.0, "BLACKFRIDAY") == 0.0

    def test_preco_zero_com_cupom_invalido(self):
        assert aplicar_cupom(0.0, "INEXISTENTE") == 0.0

    def test_aplicar_cupom_reaproveita_calcular_desconto(self):
        # Garante que aplicar_cupom é consistente com calcular_desconto.
        for codigo, percentual in CUPONS.items():
            assert aplicar_cupom(100.00, codigo) == calcular_desconto(100.00, percentual)

    def test_preco_negativo_rejeita(self):
        with pytest.raises(ValueError, match="negativo"):
            aplicar_cupom(-5.00, "PROMO10")
