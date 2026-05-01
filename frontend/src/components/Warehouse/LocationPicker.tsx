import React, { useEffect } from 'react';
import { Select } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getAreas, getShelvesByArea, getLocationsByShelf } from '../../services/locationService';

type Props = {
  value?: string;
  onChange?: (locationId: string | undefined) => void;
};

const LocationPicker: React.FC<Props> = ({ value, onChange }) => {
  const [areaId, setAreaId] = React.useState<string | undefined>();
  const [shelfId, setShelfId] = React.useState<string | undefined>();

  const areasQuery = useQuery({
    queryKey: ['areas'],
    queryFn: getAreas,
  });

  const shelvesQuery = useQuery({
    queryKey: ['shelves', areaId],
    queryFn: () => getShelvesByArea(areaId!),
    enabled: !!areaId,
  });

  const locationsQuery = useQuery({
    queryKey: ['locations', shelfId],
    queryFn: () => getLocationsByShelf(shelfId!),
    enabled: !!shelfId,
  });

  // Reset downstream when value is cleared externally
  useEffect(() => {
    if (!value) {
      setAreaId(undefined);
      setShelfId(undefined);
    }
  }, [value]);

  const handleAreaChange = (id: string) => {
    setAreaId(id);
    setShelfId(undefined);
    onChange?.(undefined);
  };

  const handleShelfChange = (id: string) => {
    setShelfId(id);
    onChange?.(undefined);
  };

  const handleLocationChange = (id: string) => {
    onChange?.(id);
  };

  return (
    <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
      <Select
        placeholder="Select area"
        style={{ minWidth: 160 }}
        loading={areasQuery.isLoading}
        value={areaId}
        onChange={handleAreaChange}
        options={(areasQuery.data ?? []).map((a) => ({ label: `${a.name} (${a.code})`, value: a.id }))}
        allowClear
        onClear={() => { setAreaId(undefined); setShelfId(undefined); onChange?.(undefined); }}
      />
      <Select
        placeholder="Select shelf"
        style={{ minWidth: 160 }}
        disabled={!areaId}
        loading={shelvesQuery.isLoading}
        value={shelfId}
        onChange={handleShelfChange}
        options={(shelvesQuery.data ?? []).map((s) => ({ label: `${s.name} (${s.code})`, value: s.id }))}
        allowClear
        onClear={() => { setShelfId(undefined); onChange?.(undefined); }}
      />
      <Select
        placeholder="Select location"
        style={{ minWidth: 160 }}
        disabled={!shelfId}
        loading={locationsQuery.isLoading}
        value={value}
        onChange={handleLocationChange}
        options={(locationsQuery.data ?? []).map((l) => ({
          label: `${l.name} (${l.code}) — qty: ${l.currentStockQuantity}`,
          value: l.id,
        }))}
        allowClear
        onClear={() => onChange?.(undefined)}
      />
    </div>
  );
};

export default LocationPicker;
