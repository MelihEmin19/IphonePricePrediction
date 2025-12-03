-- Duplicate kontrolünü kaldır
CREATE OR REPLACE FUNCTION sp_InsertListing(
    p_spec_id INTEGER,
    p_price DECIMAL(10, 2),
    p_condition VARCHAR(20),
    p_source VARCHAR(50),
    p_url TEXT
)
RETURNS INTEGER AS $$
DECLARE
    v_new_id INTEGER;
BEGIN
    INSERT INTO listings (spec_id, price, condition, source, url, scraped_at, is_active)
    VALUES (p_spec_id, p_price, p_condition, p_source, p_url, CURRENT_TIMESTAMP, TRUE)
    RETURNING id INTO v_new_id;
    
    RETURN v_new_id;
END;
$$ LANGUAGE plpgsql;

