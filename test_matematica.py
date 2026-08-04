from matematica import somar, media


def test_somar():
    assert somar(2, 3) == 6  # BUG: deveria ser 5


def test_media():
    assert media([1, 2, 3, 4]) == 2  # BUG: deveria ser 2.5
